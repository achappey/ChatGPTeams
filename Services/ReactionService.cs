using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;

namespace achappey.ChatGPTeams.Services;

public interface IReactionService
{
    Task<Reaction> EnsureReaction(string title);
}

public class ReactionService : IReactionService
{
    private readonly IReactionsRepository _reactionRepository;

    public ReactionService(IReactionsRepository reactionRepository)
    {
        _reactionRepository = reactionRepository;
    }

    public async Task<Reaction> EnsureReaction(string title)
    {
        var item = await _reactionRepository.GetByTitle(title);

        if (item == null)
        {
            item = new Reaction()
            {
                Title = title,
            };

            item.Id = await _reactionRepository.Create(item);
        }

        return item;
    }
}
