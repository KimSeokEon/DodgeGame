using System.Collections.Generic;

// Pure ballot evaluation shared by the network controller and regression checks.
public static class RestartVoteRules
{
    public enum Result { Pending, Unanimous, Rejected, MembershipChanged }
    public static Result Evaluate(IReadOnlyDictionary<int, int> ballots, IEnumerable<int> activePlayers)
    {
        var active = new HashSet<int>(activePlayers);
        if (active.Count == 0 || active.Count != ballots.Count)
            return Result.MembershipChanged;
        foreach (int id in active)
            if (!ballots.ContainsKey(id)) return Result.MembershipChanged;
        bool pending = false;
        foreach (int choice in ballots.Values)
        {
            if (choice == 2) return Result.Rejected;
            if (choice != 1) pending = true;
        }
        return pending ? Result.Pending : Result.Unanimous;
    }
}
