using Microsoft.EntityFrameworkCore;
using NSubstitute;
using StockReplenishment.Application.DTOs;
using StockReplenishment.Application.Exceptions;
using StockReplenishment.Application.Interfaces;
using StockReplenishment.Domain.Entities;
using StockReplenishment.Domain.Enums;
using StockReplenishment.Infrastructure.Data;
using StockReplenishment.Infrastructure.Services;

namespace StockReplenishment.Tests;

public class ReplenishmentServiceTests
{
    private AppDbContext _context = null!;
    private IStockValidationQueue _queue = null!;
    private IStockAvailabilityService _stockService = null!;
    private ReplenishmentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _context.StockLocations.Add(new StockLocation { Id = 1, Code = "LINE-01", Name = "Assembly Line 01" });
        _context.SaveChanges();

        _queue = Substitute.For<IStockValidationQueue>();
        _stockService = Substitute.For<IStockAvailabilityService>();
        _service = new ReplenishmentService(_context, _queue, _stockService);
    }

    private static CreateReplenishmentRequestDto ValidCreateDto() => new()
    {
        StockLocationId = 1,
        Priority = RequestPriority.Normal,
        RequestedBy = "worker1",
        Lines = [new CreateReplenishmentLineDto { ArticleNumber = "MAT-1", Description = "Bolt", RequestedQuantity = 10 }]
    };

    [Test]
    public async Task CreateAsync_Should_Create_Draft_Request()
    {
        var result = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ReplenishmentStatus.Draft));
        Assert.That(result.Lines, Has.Count.EqualTo(1));
    }

    [Test]
    public void CreateAsync_Should_Reject_Empty_Lines()
    {
        var dto = ValidCreateDto();
        dto.Lines.Clear();

        Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(dto, CancellationToken.None));
    }

    [Test]
    public void CreateAsync_Should_Reject_Zero_Quantity()
    {
        var dto = ValidCreateDto();
        dto.Lines[0].RequestedQuantity = 0;

        Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(dto, CancellationToken.None));
    }

    [Test]
    public async Task SubmitAsync_Should_Change_Status_To_Submitted_And_Queue_Validation()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);

        var result = await _service.SubmitAsync(created.Id, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ReplenishmentStatus.Submitted));
        await _queue.Received(1).QueueAsync(created.Id);
    }

    [Test]
    public async Task SubmitAsync_Should_Reject_Non_Draft_Request()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);
        await _service.SubmitAsync(created.Id, CancellationToken.None);

        Assert.ThrowsAsync<BusinessRuleException>(() => _service.SubmitAsync(created.Id, CancellationToken.None));
    }

    [Test]
    public async Task ApproveAsync_Should_Transition_Submitted_To_Approved()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);
        await _service.SubmitAsync(created.Id, CancellationToken.None);

        var result = await _service.ApproveAsync(created.Id, new ApproveReplenishmentRequestDto { ReviewedBy = "sup" }, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ReplenishmentStatus.Approved));
    }

    [Test]
    public async Task ApproveAsync_Should_Reject_Draft_Request()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);

        Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ApproveAsync(created.Id, new ApproveReplenishmentRequestDto { ReviewedBy = "sup" }, CancellationToken.None));
    }

    [Test]
    public async Task RejectAsync_Should_Require_Reason()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);
        await _service.SubmitAsync(created.Id, CancellationToken.None);

        Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.RejectAsync(created.Id, new RejectReplenishmentRequestDto { Reason = "", ReviewedBy = "sup" }, CancellationToken.None));
    }

    [Test]
    public async Task RejectAsync_Should_Transition_Submitted_To_Rejected()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);
        await _service.SubmitAsync(created.Id, CancellationToken.None);

        var result = await _service.RejectAsync(created.Id, new RejectReplenishmentRequestDto { Reason = "No stock", ReviewedBy = "sup" }, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ReplenishmentStatus.Rejected));
        Assert.That(result.RejectionReason, Is.EqualTo("No stock"));
    }

    [Test]
    public async Task FulfillAsync_Should_Transition_Approved_To_Fulfilled_And_Store_Quantities()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);
        await _service.SubmitAsync(created.Id, CancellationToken.None);
        var approved = await _service.ApproveAsync(created.Id, new ApproveReplenishmentRequestDto { ReviewedBy = "sup" }, CancellationToken.None);

        var dto = new FulfillReplenishmentRequestDto
        {
            FulfilledBy = "wh",
            Lines = [new FulfilledLineDto { LineId = approved.Lines[0].Id, FulfilledQuantity = 5 }]
        };

        var result = await _service.FulfillAsync(created.Id, dto, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ReplenishmentStatus.Fulfilled));
        Assert.That(result.Lines[0].FulfilledQuantity, Is.EqualTo(5));
    }

    [Test]
    public async Task FulfillAsync_Should_Reject_Quantity_Exceeding_Requested()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);
        await _service.SubmitAsync(created.Id, CancellationToken.None);
        var approved = await _service.ApproveAsync(created.Id, new ApproveReplenishmentRequestDto { ReviewedBy = "sup" }, CancellationToken.None);

        var dto = new FulfillReplenishmentRequestDto
        {
            FulfilledBy = "wh",
            Lines = [new FulfilledLineDto { LineId = approved.Lines[0].Id, FulfilledQuantity = 999 }]
        };

        Assert.ThrowsAsync<BusinessRuleException>(() => _service.FulfillAsync(created.Id, dto, CancellationToken.None));
    }

    [Test]
    public async Task ValidateStockAsync_Should_Save_Validation_Result()
    {
        var created = await _service.CreateAsync(ValidCreateDto(), CancellationToken.None);
        await _service.SubmitAsync(created.Id, CancellationToken.None);

        _stockService
            .CheckAvailabilityAsync(Arg.Any<ReplenishmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StockAvailabilityResult { IsAvailable = true, Message = "Stock available" });

        await _service.ValidateStockAsync(created.Id, CancellationToken.None);

        var validation = await _service.GetValidationAsync(created.Id, CancellationToken.None);

        Assert.That(validation.Status, Is.EqualTo(StockValidationStatus.Completed));
        Assert.That(validation.IsAvailable, Is.True);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }
}
