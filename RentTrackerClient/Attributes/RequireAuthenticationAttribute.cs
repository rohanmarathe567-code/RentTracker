using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Security.Claims;
using RentTrackerClient.Shared;

namespace RentTrackerClient.Attributes;

public class RequireAuthenticationAttribute : AuthorizeAttribute
{
    public RequireAuthenticationAttribute()
    {
    }
}

// Component that will be rendered when authorization fails
public class RedirectToLoginComponent : ComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
        builder.OpenComponent<RedirectToLogin>(0);
        builder.CloseComponent();
    }
}