using System;
using System.Collections.Generic;
using System.Linq;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// A double tournament selection strategy that combats bloat.
    /// It first runs a parsimony tournament (preferring smaller complexity),
    /// then runs a fitness tournament on the winners.
    /// </summary>
    public class DoubleTournamentSelection : ISelectionStrategy
    {
        public int ParsimonyTournamentSize { get; }
        public int FitnessTournamentSize { get; }
        private readonly Random _rng;

        public DoubleTournamentSelection(int parsimonySize = 7, int fitnessSize = 7, int? seed = null)
        {
            ParsimonyTournamentSize = parsimonySize;
            FitnessTournamentSize = fitnessSize;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public IReadOnlyList<SelectionCandidate> Select(IReadOnlyList<SelectionCandidate> population, int count)
        {
            if (population == null || population.Count == 0)
                throw new ArgumentException("Population cannot be empty.", nameof(population));

            var results = new List<SelectionCandidate>(count);
            for (int i = 0; i < count; i++)
            {
                results.Add(RunDoubleTournament(population));
            }

            return results;
        }

        private SelectionCandidate RunDoubleTournament(IReadOnlyList<SelectionCandidate> pop)
        {
            // First stage: Parsimony tournament. Generate N winners biased towards smaller complexity.
            var fitnessContestants = new List<SelectionCandidate>(FitnessTournamentSize);
            for (int i = 0; i < FitnessTournamentSize; i++)
            {
                var parsimonyWinner = RunSingleTournament(pop, ParsimonyTournamentSize, byComplexity: true);
                fitnessContestants.Add(parsimonyWinner);
            }

            // Second stage: Fitness tournament among the smaller candidates.
            return RunSingleTournament(fitnessContestants, FitnessTournamentSize, byComplexity: false);
        }

        private SelectionCandidate RunSingleTournament(IReadOnlyList<SelectionCandidate> pop, int size, bool byComplexity)
        {
            SelectionCandidate best = pop[_rng.Next(pop.Count)];
            for (int i = 1; i < size; i++)
            {
                var contestant = pop[_rng.Next(pop.Count)];
                if (byComplexity)
                {
                    if (contestant.Complexity < best.Complexity) best = contestant;
                }
                else
                {
                    // Lower fitness (MSE) is better
                    if (contestant.Fitness < best.Fitness) best = contestant;
                }
            }
            return best;
        }
    }
}
