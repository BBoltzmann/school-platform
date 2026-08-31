namespace SchoolPlatform.Application.Platform;

public interface ISchoolBootstrapService
{
    Task<BootstrapSchoolResult> BootstrapAsync(
        BootstrapSchoolRequest request,
        CancellationToken cancellationToken = default);
}
