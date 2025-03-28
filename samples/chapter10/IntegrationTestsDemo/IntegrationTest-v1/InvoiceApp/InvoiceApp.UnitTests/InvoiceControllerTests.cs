﻿using InvoiceApp.WebApi;
using InvoiceApp.WebApi.Controllers;
using InvoiceApp.WebApi.Interfaces;
using InvoiceApp.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InvoiceApp.UnitTests;

public class InvoiceControllerTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public InvoiceControllerTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetInvoices_ShouldReturnInvoices()
    {
        // Arrange
        var repositoryMock = new Mock<IInvoiceRepository>();

        repositoryMock.Setup(x => x.GetInvoicesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<InvoiceStatus?>()))
            .ReturnsAsync((int page, int pageSize, InvoiceStatus? status) =>
                _fixture.Invoices.Where(x => status == null || x.Status == status)
                    .OrderByDescending(x => x.InvoiceDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList());
        var emailServiceMock = new Mock<IEmailService>();
        var controller = new InvoiceController(repositoryMock.Object, emailServiceMock.Object);
        // Act
        var actionResult = await controller.GetInvoicesAsync();
        // Assert
        var result = actionResult.Result as OkObjectResult;
        Assert.NotNull(result);
        var returnResult = Assert.IsAssignableFrom<List<Invoice>>(result.Value);
        Assert.NotNull(returnResult);
        Assert.Equal(2, returnResult.Count);
        Assert.Contains(returnResult, i => i.InvoiceNumber == "INV-001");
        Assert.Contains(returnResult, i => i.InvoiceNumber == "INV-002");
    }

    [Fact]
    public async Task GetInvoice_ShouldReturnInvoice()
    {
        // Arrange
        var repositoryMock = new Mock<IInvoiceRepository>();
        repositoryMock.Setup(x => x.GetInvoiceAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _fixture.Invoices.FirstOrDefault(x => x.Id == id));
        var emailServiceMock = new Mock<IEmailService>();
        var controller = new InvoiceController(repositoryMock.Object, emailServiceMock.Object);
        // Act
        var invoice = _fixture.Invoices.First();
        var actionResult = await controller.GetInvoiceAsync(invoice.Id);
        // Assert
        var result = actionResult.Result as OkObjectResult;
        Assert.NotNull(result);
        var returnResult = Assert.IsAssignableFrom<Invoice>(result.Value);
        Assert.NotNull(returnResult);
        Assert.Equal(invoice.Id, returnResult.Id);
        Assert.Equal(invoice.InvoiceNumber, returnResult.InvoiceNumber);
    }

    [Fact]
    public async Task GetInvoice_ShouldReturnNotFound()
    {
        // Arrange
        var repositoryMock = new Mock<IInvoiceRepository>();
        repositoryMock.Setup(x => x.GetInvoiceAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _fixture.Invoices.FirstOrDefault(x => x.Id == id));
        var emailServiceMock = new Mock<IEmailService>();
        var controller = new InvoiceController(repositoryMock.Object, emailServiceMock.Object);
        // Act
        var actionResult = await controller.GetInvoiceAsync(Guid.NewGuid());
        // Assert
        var result = actionResult.Result as NotFoundResult;
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(InvoiceStatus.AwaitPayment)]
    [InlineData(InvoiceStatus.Draft)]
    public async Task GetInvoicesByStatus_ShouldReturnInvoices(InvoiceStatus status)
    {
        // Arrange
        var repositoryMock = new Mock<IInvoiceRepository>();
        repositoryMock.Setup(x => x.GetInvoicesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<InvoiceStatus?>()))
            .ReturnsAsync((int page, int pageSize, InvoiceStatus? invoiceStatus) =>
                _fixture.Invoices.Where(x => invoiceStatus == null || x.Status == invoiceStatus)
                    .OrderByDescending(x => x.InvoiceDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList());
        var emailServiceMock = new Mock<IEmailService>();
        var controller = new InvoiceController(repositoryMock.Object, emailServiceMock.Object);
        // Act
        var actionResult = await controller.GetInvoicesAsync(status: status);
        // Assert
        var result = actionResult.Result as OkObjectResult;
        Assert.NotNull(result);
        var returnResult = Assert.IsAssignableFrom<List<Invoice>>(result.Value);
        Assert.NotNull(returnResult);
        Assert.Single(returnResult);
        Assert.Equal(status, returnResult.First().Status);
    }

    [Fact]
    public async Task CreateInvoice_ShouldCreateInvoice()
    {
        // Arrange
        var repositoryMock = new Mock<IInvoiceRepository>();
        repositoryMock.Setup(x => x.CreateInvoiceAsync(It.IsAny<Invoice>()))
            .ReturnsAsync((Invoice invoice) =>
            {
                _fixture.Invoices.Add(invoice);
                invoice.Id = Guid.NewGuid();
                return invoice;
            });
        var emailServiceMock = new Mock<IEmailService>();
        var controller = new InvoiceController(repositoryMock.Object, emailServiceMock.Object);
        // Act
        var invoice = new Invoice
        {
            DueDate = DateTimeOffset.Now.AddDays(30),
            ContactId = _fixture.Contacts.First().Id,
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTimeOffset.Now,
            InvoiceItems = new List<InvoiceItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = "Item 1",
                    Quantity = 1,
                    UnitPrice = 100,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = "Item 2",
                    Quantity = 2,
                    UnitPrice = 200,
                }
            }
        };
        var actionResult = await controller.CreateInvoiceAsync(invoice);
        // Assert
        var result = actionResult.Result as CreatedAtActionResult;
        Assert.NotNull(result);
        var returnResult = Assert.IsAssignableFrom<Invoice>(result.Value);
        var invoiceCreated = _fixture.Invoices.FirstOrDefault(x => x.Id == returnResult.Id);

        Assert.NotNull(invoiceCreated);
        Assert.Equal(InvoiceStatus.Draft, invoiceCreated.Status);
        Assert.Equal(500, invoiceCreated.Amount);
        Assert.Equal(3, _fixture.Invoices.Count());
        // You can add more assertions here

        // Clean up
        _fixture.Invoices.Remove(invoiceCreated);
    }

    [Fact]
    public async Task UpdateInvoice_ShouldUpdateInvoice()
    {
        // Arrange
        var repositoryMock = new Mock<IInvoiceRepository>();
        repositoryMock.Setup(x => x.UpdateInvoiceAsync(It.IsAny<Invoice>()))
            .ReturnsAsync((Invoice invoice) =>
            {
                var invoiceToUpdate = _fixture.Invoices.FirstOrDefault(x => x.Id == invoice.Id);
                if (invoiceToUpdate != null)
                {
                    invoiceToUpdate.Status = invoice.Status;
                    invoiceToUpdate.Description = invoice.Description;
                    invoiceToUpdate.InvoiceItems = invoice.InvoiceItems;
                    invoiceToUpdate.Amount = invoice.Amount;
                }
                return invoiceToUpdate;
            });
        repositoryMock.Setup(x => x.GetInvoiceAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _fixture.Invoices.FirstOrDefault(x => x.Id == id));
        var emailServiceMock = new Mock<IEmailService>();
        var controller = new InvoiceController(repositoryMock.Object, emailServiceMock.Object);
        // Act
        var invoice = _fixture.Invoices.First();
        invoice.Status = InvoiceStatus.Paid;
        invoice.Description = "Updated description";
        invoice.InvoiceItems.ForEach(x =>
        {
            x.Description = "Updated description";
            x.UnitPrice += 100;
        });
        var expectedAmount = invoice.InvoiceItems.Sum(x => x.UnitPrice * x.Quantity);
        await controller.UpdateInvoiceAsync(invoice.Id, invoice);

        // Assert
        var invoiceUpdated = _fixture.Invoices.FirstOrDefault(x => x.Id == invoice.Id);
        Assert.NotNull(invoiceUpdated);
        Assert.Equal(InvoiceStatus.Paid, invoiceUpdated.Status);
        Assert.Equal("Updated description", invoiceUpdated.Description);
        Assert.Equal(expectedAmount, invoiceUpdated.Amount);
        Assert.Equal(2, _fixture.Invoices.Count);
        // Restore the mock database
        _fixture.InitializeDatabase();
    }
}
