using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using System.Threading.Tasks;

namespace achappey.ChatGPTeams.Repositories;

public interface IFilesRepository
{
    Task<File> UploadFile(string purpose, byte[] file, string fileName);
    Task<IEnumerable<File>> ListFiles();
}

public class FilesRepository : IFilesRepository
{
    private readonly ILogger<ChatRepository> _logger;
    private readonly IMapper _mapper;
    private readonly OpenAIService _openAIService;

    public FilesRepository(ILogger<ChatRepository> logger,
    OpenAIService openAIService, IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _openAIService = openAIService;
    }

    public async Task<File> UploadFile(string purpose, byte[] file, string fileName)
    {
        var result = await _openAIService.UploadFile(purpose, file, fileName);

        if (result.Successful)
        {
            return new File()
            {
                Id = result.Id,
                FileName = result.FileName,
            };
        }

        throw new System.Exception(result.Error.Message);
    }

    public async Task<IEnumerable<File>> ListFiles()
    {
        var result = await _openAIService.ListFile();

        if (result.Successful)
        {
            return result.Data.Select(a => new File()
            {
                Id = a.Id,
                FileName = a.FileName,
            });
        }

        throw new System.Exception(result.Error.Message);
    }
}