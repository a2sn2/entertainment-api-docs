using Athar.Domain;
using Xunit;

namespace Athar.Tests;

public sealed class InitiativeTests
{
    [Fact]
    public void Valid_initiative_starts_submitted_and_raises_event()
    {
        var now = new DateTimeOffset(
            2026,
            8,
            6,
            15,
            0,
            0,
            TimeSpan.Zero);

        var result = Initiative.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "مختبر تعلّم متنقل",
            "مبادرة توصل جلسات تعليم رقمية عملية إلى المدارس الواقعة خارج مراكز المدن.",
            "تعليم",
            "صنعاء",
            25_000,
            320,
            now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitiativeStatuses.Submitted, result.Value.Status);
        Assert.Equal(now, result.Value.CreatedUtc);
        Assert.Single(result.Value.DomainEvents);
    }

    [Fact]
    public void Administrator_can_approve_submitted_initiative()
    {
        var creation = Initiative.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "حديقة بيانات الحي",
            "مبادرة تجمع بيانات البيئة المحلية وتعرضها للسكان بطريقة مبسطة ومفتوحة.",
            "بيئة",
            "تعز",
            12_000,
            800,
            DateTimeOffset.UtcNow);

        var review = creation.Value.Review(
            InitiativeDecisions.Approve,
            Guid.NewGuid(),
            "المبادرة واضحة وقابلة للقياس.",
            DateTimeOffset.UtcNow.AddMinutes(3));

        Assert.True(review.IsSuccess);
        Assert.Equal(InitiativeStatuses.Approved, creation.Value.Status);
        Assert.Equal(2, creation.Value.DomainEvents.Count);
    }

    [Fact]
    public void Owner_cannot_review_own_initiative_even_when_authorized_as_administrator()
    {
        var ownerId = Guid.NewGuid();
        var initiative = Initiative.Create(
            Guid.NewGuid(),
            ownerId,
            "مساحة أحياء رقمية",
            "مبادرة تربط سكان الحي بخدمات تطوعية ومعلومات موثوقة عبر منصة رقمية محلية.",
            "مجتمع",
            "صنعاء",
            18_000,
            450,
            DateTimeOffset.UtcNow).Value;

        var review = initiative.Review(
            InitiativeDecisions.Approve,
            ownerId,
            "محاولة مراجعة ذاتية يجب رفضها.",
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.True(review.IsFailure);
        Assert.Equal("Athar.SelfReviewNotAllowed", review.Error.Code);
        Assert.Equal(InitiativeStatuses.Submitted, initiative.Status);
        Assert.Single(initiative.DomainEvents);
    }

    [Fact]
    public void Final_initiative_cannot_be_reviewed_twice()
    {
        var initiative = Initiative.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "مسار تمكين الحرفيات",
            "برنامج يربط الحرفيات بمتاجر رقمية ويقدم لهن تدريبًا عمليًا على التسعير والتسويق.",
            "تمكين اقتصادي",
            "إب",
            40_000,
            120,
            DateTimeOffset.UtcNow).Value;

        Assert.True(initiative.Review(
            InitiativeDecisions.Reject,
            Guid.NewGuid(),
            "تحتاج المبادرة إلى خطة استدامة.",
            DateTimeOffset.UtcNow).IsSuccess);

        var second = initiative.Review(
            InitiativeDecisions.Approve,
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.True(second.IsFailure);
        Assert.Equal("Athar.AlreadyReviewed", second.Error.Code);
    }
}
