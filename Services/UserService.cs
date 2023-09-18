using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;

namespace achappey.ChatGPTeams.Services;


public interface IUserService
{
    Task<User> GetCurrentUser();
    Task<IEnumerable<User>> GetAll();
    Task<User> GetUser(string id);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public UserService(IUserRepository userRepository, IDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<IEnumerable<User>> GetAll()
    {
        return await _userRepository.GetAll();
    }

    public async Task<User> GetUser(string id)
    {
        return await _userRepository.Get(id);
    }

    public async Task<User> GetCurrentUser()
    {
        return await _userRepository.GetCurrent();
    }
}

