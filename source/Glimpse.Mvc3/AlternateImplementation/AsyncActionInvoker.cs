﻿using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using Glimpse.Core;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.Core.Message;

namespace Glimpse.Mvc.AlternateImplementation
{
    public class AsyncActionInvoker : Alternate<AsyncControllerActionInvoker>
    {
        public AsyncActionInvoker(IProxyFactory proxyFactory) : base(proxyFactory)
        {
        }

        public override IEnumerable<IAlternateImplementation<AsyncControllerActionInvoker>> AllMethods()
        {
            yield return new BeginInvokeActionMethod();
            yield return new EndInvokeActionMethod();
            yield return new ActionInvoker.InvokeActionResult<AsyncControllerActionInvoker>();
            yield return new ActionInvoker.GetFilters<AsyncControllerActionInvoker>();
        }

        public class BeginInvokeActionMethod : IAlternateImplementation<AsyncControllerActionInvoker>
        {
            public BeginInvokeActionMethod()
            {
                MethodToImplement = typeof(AsyncControllerActionInvoker).GetMethod("BeginInvokeActionMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            public MethodInfo MethodToImplement { get; private set; }

            public void NewImplementation(IAlternateImplementationContext context)
            {
                // BeginInvokeActionMethod(ControllerContext controllerContext, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters, AsyncCallback callback, object state)
                if (context.RuntimePolicyStrategy() == RuntimePolicy.Off)
                {
                    context.Proceed();
                    return;
                }

                var state = (IActionInvokerStateMixin)context.Proxy;
                var timer = context.TimerStrategy();
                state.Arguments = new ActionInvoker.InvokeActionMethod.Arguments(context.Arguments);
                state.Offset = timer.Start();
                context.Proceed();
            }
        }

        public class EndInvokeActionMethod : IAlternateImplementation<AsyncControllerActionInvoker>
        {
            public EndInvokeActionMethod()
            {
                MethodToImplement = typeof(AsyncControllerActionInvoker).GetMethod("EndInvokeActionMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            public MethodInfo MethodToImplement { get; private set; }

            public void NewImplementation(IAlternateImplementationContext context)
            {
                if (context.RuntimePolicyStrategy() == RuntimePolicy.Off)
                {
                    context.Proceed();
                    return;
                }

                context.Proceed();
                var state = (IActionInvokerStateMixin)context.Proxy;
                var timer = context.TimerStrategy();
                var timerResult = timer.Stop(state.Offset);

                var eventName = string.Format(
                    "{0}.{1}()",
                    state.Arguments.ActionDescriptor.ControllerDescriptor.ControllerName,
                    state.Arguments.ActionDescriptor.ActionName);
                
                context.MessageBroker.PublishMany(
                    new ActionInvoker.InvokeActionMethod.Message(state.Arguments, (ActionResult)context.ReturnValue),
                    new TimerResultMessage(timerResult, eventName, "ASP.NET MVC")); // TODO: This should be abstracted
            }
        }
    }
}