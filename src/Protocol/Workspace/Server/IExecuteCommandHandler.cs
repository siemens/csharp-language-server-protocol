using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

// ReSharper disable CheckNamespace

namespace OmniSharp.Extensions.LanguageServer.Protocol.Server
{
    [Serial, Method(WorkspaceNames.ExecuteCommand)]
    public interface IExecuteCommandHandler : IJsonRpcRequestHandler<ExecuteCommandParams>, IRegistration<ExecuteCommandRegistrationOptions>, ICapability<ExecuteCommandCapability> { }

    public abstract class ExecuteCommandHandler : IExecuteCommandHandler
    {
        private readonly ExecuteCommandRegistrationOptions _options;
        private readonly ProgressManager _progressManager;

        public ExecuteCommandHandler(ExecuteCommandRegistrationOptions registrationOptions, ProgressManager progressManager)
        {
            _options = registrationOptions;
            _progressManager = progressManager;
        }

        public ExecuteCommandRegistrationOptions GetRegistrationOptions() => _options;

        public async Task<Unit> Handle(
            ExecuteCommandParams request,
            CancellationToken cancellationToken)
        {
            using var progressReporter = _progressManager.Delegate(request, cancellationToken);
            return await Handle(request, progressReporter, cancellationToken).ConfigureAwait(false);
        }

        public abstract Task<Unit> Handle(ExecuteCommandParams request, WorkDoneProgressReporter progressReporter, CancellationToken cancellationToken);

        public virtual void SetCapability(ExecuteCommandCapability capability) => Capability = capability;
        protected ExecuteCommandCapability Capability { get; private set; }
    }

    public static class ExecuteCommandHandlerExtensions
    {
        public static IDisposable OnExecuteCommand(
            this ILanguageServerRegistry registry,
            Func<ExecuteCommandParams, WorkDoneProgressReporter, CancellationToken, Task<Unit>> handler,
            ExecuteCommandRegistrationOptions registrationOptions = null,
            Action<ExecuteCommandCapability> setCapability = null)
        {
            registrationOptions ??= new ExecuteCommandRegistrationOptions();
            return registry.AddHandlers(new DelegatingHandler(handler, registry.ProgressManager, setCapability, registrationOptions));
        }

        class DelegatingHandler : ExecuteCommandHandler
        {
            private readonly Func<ExecuteCommandParams, WorkDoneProgressReporter, CancellationToken, Task<Unit>> _handler;
            private readonly Action<ExecuteCommandCapability> _setCapability;

            public DelegatingHandler(Func<ExecuteCommandParams, WorkDoneProgressReporter, CancellationToken, Task<Unit>> handler,
                ProgressManager progressManager,
                Action<ExecuteCommandCapability> setCapability,
                ExecuteCommandRegistrationOptions registrationOptions) : base(registrationOptions, progressManager)
            {
                _handler = handler;
                _setCapability = setCapability;
            }

            public override Task<Unit> Handle(ExecuteCommandParams request, WorkDoneProgressReporter progressReporter, CancellationToken cancellationToken) => _handler.Invoke(request, progressReporter, cancellationToken);
            public override void SetCapability(ExecuteCommandCapability capability) => _setCapability?.Invoke(capability);

        }
    }
}
