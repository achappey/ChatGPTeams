using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;

namespace achappey.ChatGPTeams.Services;

public interface IDepartmentService
{
    Task<Department> GetDepartmentAsync(string id);
    Task<Department> GetDepartmentByNameAsync(string name);
    Task<IEnumerable<Department>> GetAllDepartmentsAsync();
}

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Department> GetDepartmentAsync(string id)
    {
        return await _departmentRepository.Get(id);
    }

    public async Task<Department> GetDepartmentByNameAsync(string name)
    {
        return await _departmentRepository.GetByName(name);
    }

    public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
    {
        return await _departmentRepository.GetAll();
    }

}
