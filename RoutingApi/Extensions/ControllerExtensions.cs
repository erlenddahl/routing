using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace RoutingApi.Extensions
{
    public static class ControllerExtensions
    {
        public static void CheckModelState(this Controller ctrl)
        {
            if (!ctrl.ModelState.IsValid)
            {
                throw new Exception("Invalid JSON: " + string.Join(Environment.NewLine, ctrl.ModelState.Values.Select(p => string.Join("; ", p.Errors.Select(c => c.ErrorMessage)))));
            }
        }
    }
}
