using Xunit;
using SymbolicRegressionNet.Sdk.Reporting;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class ExpressionExporterTests
    {
        [Fact]
        public void PythonExporter_FormatsMathFunctionsAndVariablesCorrectly()
        {
            var exporter = new PythonExporter();
            var expr = "sin(x0) + cos(x1) * exp(x2)";
            var result = exporter.Export(expr, new[] { "Age", "Weight", "Height" });
            Assert.Equal("np.sin(Age) + np.cos(Weight) * np.exp(Height)", result);
        }

        [Fact]
        public void CCodeExporter_FormatsMathFunctionsAndVariablesCorrectly()
        {
            var exporter = new CCodeExporter();
            var expr = "sin(x0) + pow(x1, 2) * exp(x2)";
            var result = exporter.Export(expr, new[] { "A", "B", "C" });
            Assert.Equal("std::sin(A) + std::pow(B, 2) * std::exp(C)", result);
        }

        [Fact]
        public void LatexExporter_FormatsMathFunctionsAndVariablesCorrectly()
        {
            var exporter = new LatexExporter();
            var expr = "sin(x0) * 2.5";
            var result = exporter.Export(expr, new[] { "Alpha" });
            Assert.Contains(@"\sin(Alpha)", result);
            Assert.Contains(@"\cdot", result);
        }

        [Fact]
        public void Exporters_HandleNullVariablesGracefully()
        {
            var python = new PythonExporter();
            var result = python.Export("sin(x0)", null);
            Assert.Equal("np.sin(x0)", result);
        }
    }
}
