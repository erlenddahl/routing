using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace RoutingApi.Extensions
{
    public static class ControllerExtensions
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static void CheckModelState(this Controller ctrl)
        {
            if (!ctrl.ModelState.IsValid)
            {
                logger.Info("Invalid model: " + ctrl.ModelState.ValidationState + " (" + ctrl.ModelState.ErrorCount + ")");
                throw new Exception("Invalid JSON: " + string.Join(Environment.NewLine, ctrl.ModelState.Values.Select(p => string.Join("; ", p.Errors.Select(c => c.ErrorMessage)))));
            }
        }
    }
}
