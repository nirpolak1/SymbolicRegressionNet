#nullable disable
using System.Collections.Generic;
using Xunit;
using SymbolicRegressionNet.Sdk.Api;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class SelectionTests
    {
        [Fact]
        public void DoubleTournamentSelection_PrefersParsimoniousAndFitCandidates()
        {
            // Seeded RNG to ensure deterministic test
            var selection = new DoubleTournamentSelection(parsimonySize: 3, fitnessSize: 3, seed: 42);

            var pop = new List<SelectionCandidate>
            {
                new SelectionCandidate("x0 + 1", 0.5, 3),        // Fit but somewhat small
                new SelectionCandidate("x0 + x0 + x0", 0.1, 15), // Very fit but heavily bloated
                new SelectionCandidate("x0", 1.5, 1),            // Very small but unfit
                new SelectionCandidate("x1 * 0", 2.0, 3)         // Bad fitness
            };

            var winners = selection.Select(pop, 100);

            int bloatedWinners = 0;
            int balancedWinners = 0;

            foreach (var w in winners)
            {
                if (w.Complexity == 15) bloatedWinners++;
                if (w.Complexity == 3 && w.Fitness == 0.5) balancedWinners++;
            }

            // Expected behavior: The highly bloated candidate (complexity 15) is killed off
            // in the parsimony tournament before it can dominate the fitness tournament.
            // Therefore, the balanced candidate should win a vast majority of the time.
            Assert.True(balancedWinners > bloatedWinners);
            Assert.True(bloatedWinners < 20); // Bloated should rarely survive
        }
    }
}
