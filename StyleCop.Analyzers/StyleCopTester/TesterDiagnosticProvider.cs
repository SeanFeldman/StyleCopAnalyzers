﻿namespace StyleCopTester
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;

    internal sealed class TesterDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private readonly ImmutableDictionary<string, ImmutableArray<Diagnostic>> documentDiagnostics;
        private readonly ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> projectDiagnostics;

        public TesterDiagnosticProvider(ImmutableDictionary<string, ImmutableArray<Diagnostic>> documentDiagnostics, ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> projectDiagnostics)
        {
            this.documentDiagnostics = documentDiagnostics;
            this.projectDiagnostics = projectDiagnostics;
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.projectDiagnostics.Values.SelectMany(i => i).Concat(this.documentDiagnostics.Values.SelectMany(i => i)));
        }

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            ImmutableArray<Diagnostic> diagnostics;
            if (!this.documentDiagnostics.TryGetValue(document.FilePath, out diagnostics))
            {
                return Task.FromResult(Enumerable.Empty<Diagnostic>());
            }

            return Task.FromResult(diagnostics.AsEnumerable());
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            ImmutableArray<Diagnostic> diagnostics;
            if (!this.projectDiagnostics.TryGetValue(project.Id, out diagnostics))
            {
                return Task.FromResult(Enumerable.Empty<Diagnostic>());
            }

            return Task.FromResult(diagnostics.AsEnumerable());
        }
    }
}