using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

// ReSharper disable CheckNamespace

namespace OmniSharp.Extensions.LanguageServer.Protocol.Server
{
    [Parallel, Method(DocumentNames.CodeLens)]
    public interface ICodeLensHandler : IJsonRpcRequestHandler<CodeLensParams, CodeLensContainer>, IRegistration<CodeLensRegistrationOptions>, ICapability<CodeLensCapability> { }

    [Parallel, Method(DocumentNames.CodeLensResolve)]
    public interface ICodeLensResolveHandler : ICanBeResolvedHandler<CodeLens> { }

    public abstract class CodeLensHandler : ICodeLensHandler, ICodeLensResolveHandler
    {
        private readonly CodeLensRegistrationOptions _options;
        private readonly ProgressManager _progressManager;
        public CodeLensHandler(CodeLensRegistrationOptions registrationOptions, ProgressManager progressManager)
        {
            _options = registrationOptions;
            _progressManager = progressManager;
        }

        public CodeLensRegistrationOptions GetRegistrationOptions() => _options;

        public async Task<CodeLensContainer> Handle(CodeLensParams request, CancellationToken cancellationToken)
        {
            using var partialResults = _progressManager.For(request, cancellationToken);
            using var progressReporter = _progressManager.Delegate(request, cancellationToken);
            return await Handle(request, partialResults, progressReporter, cancellationToken).ConfigureAwait(false);
        }

        public abstract Task<CodeLensContainer> Handle(
            CodeLensParams request,
            IObserver<CodeLensContainer> partialResults,
            WorkDoneProgressReporter progressReporter,
            CancellationToken cancellationToken
        );

        public abstract Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken);
        public abstract bool CanResolve(CodeLens value);
        public virtual void SetCapability(CodeLensCapability capability) => Capability = capability;
        protected CodeLensCapability Capability { get; private set; }
    }

    public static class CodeLensHandlerExtensions
    {
        public static IDisposable OnCodeLens(
            this ILanguageServerRegistry registry,
            Func<CodeLensParams, IObserver<CodeLensContainer>, WorkDoneProgressReporter, CancellationToken, Task<CodeLensContainer>> handler,
            Func<CodeLens, CancellationToken, Task<CodeLens>> resolveHandler = null,
            Func<CodeLens, bool> canResolve = null,
            CodeLensRegistrationOptions registrationOptions = null,
            Action<CodeLensCapability> setCapability = null)
        {
            var codeLensRegistrationOptions = new CodeLensRegistrationOptions();
            codeLensRegistrationOptions.ResolveProvider = canResolve != null && resolveHandler != null;
            if (registrationOptions != null)
            {
                codeLensRegistrationOptions.DocumentSelector = registrationOptions.DocumentSelector;
            }
            return registry.AddHandlers(new DelegatingHandler(handler, resolveHandler, registry.ProgressManager, canResolve, setCapability, codeLensRegistrationOptions));
        }

        class DelegatingHandler : CodeLensHandler
        {
            private readonly Func<CodeLensParams, IObserver<CodeLensContainer>, WorkDoneProgressReporter, CancellationToken, Task<CodeLensContainer>> _handler;
            private readonly Func<CodeLens, CancellationToken, Task<CodeLens>> _resolveHandler;
            private readonly Func<CodeLens, bool> _canResolve;
            private readonly Action<CodeLensCapability> _setCapability;

            public DelegatingHandler(
                Func<CodeLensParams, IObserver<CodeLensContainer>, WorkDoneProgressReporter, CancellationToken, Task<CodeLensContainer>> handler,
                Func<CodeLens, CancellationToken, Task<CodeLens>> resolveHandler,
                ProgressManager progressManager,
                Func<CodeLens, bool> canResolve,
                Action<CodeLensCapability> setCapability,
                CodeLensRegistrationOptions registrationOptions) : base(registrationOptions, progressManager)
            {
                _handler = handler;
                _resolveHandler = resolveHandler;
                _canResolve = canResolve;
                _setCapability = setCapability;
            }

            public override Task<CodeLensContainer> Handle(
                CodeLensParams request,
                IObserver<CodeLensContainer> partialResults,
                WorkDoneProgressReporter progressReporter,
                CancellationToken cancellationToken
            ) => _handler.Invoke(request, partialResults, progressReporter, cancellationToken);
            public override Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken) => _resolveHandler.Invoke(request, cancellationToken);
            public override bool CanResolve(CodeLens value) => _canResolve.Invoke(value);
            public override void SetCapability(CodeLensCapability capability) => _setCapability?.Invoke(capability);

        }
    }
}
