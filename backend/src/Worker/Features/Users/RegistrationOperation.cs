using Kaban.Worker.Infrastructure.Database.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Worker.Features.Users;

public sealed class RegistrationOperation
{
    public Guid Id { get; set; }
    public required string Fingerprint { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
public sealed class RegistrationOperationConfiguration : IEntityTypeConfiguration<RegistrationOperation>
{
    public void Configure(EntityTypeBuilder<RegistrationOperation> builder)
    {
        builder.ToTable("registration_operations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Fingerprint).HasMaxLength(64).IsRequired();
        builder.HasOne<Infrastructure.Database.Domain.Users>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

