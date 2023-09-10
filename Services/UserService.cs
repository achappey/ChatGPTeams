using System.Collections;
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
    //Task<User> GetByAadObjectId(string id);
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
        //   return await WithDepartment(item);
    }

    public async Task<User> GetUser(string id)
    {
        var item = await _userRepository.Get(id);
        return item;
        //   return await WithDepartment(item);
    }

    public async Task<User> GetCurrentUser()
    {
        var user = await _userRepository.GetCurrent();
        return user;
        //    if (user.Department != null)
        //  {
        //      user.Department = await _departmentRepository.GetByName(user.Department.Name);
        // }

        //  return await WithDepartment(user);
    }

    /*  private async Task<User> WithDepartment(User user)
      {
          if (user.Department != null)
          {
              user.Department = await _departmentRepository.GetByName(user.Department.Name);
          }

          return user;
      }*/

    //  public async Task<User> Get(int id)
    //  {
    //      return await WithDepartment(await _userRepository.Get(id));
    // }

}
