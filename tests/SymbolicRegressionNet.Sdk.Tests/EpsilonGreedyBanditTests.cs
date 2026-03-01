#nullable disable
using Xunit;
using SymbolicRegressionNet.Sdk.Api;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class EpsilonGreedyBanditTests
    {
        [Fact]
        public void Bandit_LearnsToExploitHighestRewardingOperator()
        {
            // Epsilon 0.0 means purely greedy after initial values
            var bandit = new EpsilonGreedyBandit(epsilon: 0.0, seed: 42);

            // Let's say SubtreeMutation is generally best (reward 10.0), Crossover is bad (reward 1.0)
            bandit.ObserveReward(GeneticOperator.SubtreeMutation, 10.0);
            bandit.ObserveReward(GeneticOperator.Crossover, 1.0);
            bandit.ObserveReward(GeneticOperator.PointMutation, 2.0);
            bandit.ObserveReward(GeneticOperator.HoistMutation, 1.5);

            int crossoverPicks = 0;
            int subtreePicks = 0;

            for (int i = 0; i < 1000; i++)
            {
                var op = bandit.SelectOperator();
                if (op == GeneticOperator.Crossover) crossoverPicks++;
                if (op == GeneticOperator.SubtreeMutation) subtreePicks++;

                // Give small noise
                if (op == GeneticOperator.SubtreeMutation) bandit.ObserveReward(op, 10.0);
            }

            // SubtreeMutation should dominate completely
            Assert.True(subtreePicks > 900);
            Assert.Equal(0, crossoverPicks);

            // Probabilities output should heavily favor SubtreeMutation
            var probs = bandit.GetProbabilities();
            Assert.True(probs[GeneticOperator.SubtreeMutation] > 0.6);
            Assert.True(probs[GeneticOperator.Crossover] < 0.2);
        }
    }
}
