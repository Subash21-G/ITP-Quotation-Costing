using ITPQuotation.Api.Controllers;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ITPQuotation.Api.Common;

// Authentication is enforced by the fallback policy. Keep read-only access
// available to viewers while requiring an estimating role for business writes.
public sealed class BusinessAccessFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor action ||
            action.ControllerTypeInfo.AsType() == typeof(AuthController)) return;

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var method = context.HttpContext.Request.Method;
        var readOnly = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) ||
            action.ControllerTypeInfo.AsType() == typeof(CalculationsController);
        if (!readOnly && !user.IsInRole(AuthService.Admin) && !user.IsInRole(AuthService.Estimator))
            context.Result = new ForbidResult();
    }
}
