using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using AutoMapper;

namespace achappey.ChatGPTeams.Services;

public interface IDepartmentService
{
    Task<Department> GetDepartmentAsync(int id);
    Task<Department> GetDepartmentByNameAsync(string name);
    Task<IEnumerable<Department>> GetAllDepartmentsAsync();
}

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IMapper _mapper;

    public DepartmentService(IDepartmentRepository departmentRepository, IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
    }

    public async Task<Department> GetDepartmentAsync(int id)
    {
        var item = await _departmentRepository.Get(id);

        return _mapper.Map<Department>(item);
    }

    public async Task<Department> GetDepartmentByNameAsync(string name)
    {
        var item = await _departmentRepository.GetByName(name);

        return _mapper.Map<Department>(item);
    }

    public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
    {
        var items = await _departmentRepository.GetAll();

        return items.Select(_mapper.Map<Department>);
    }

}
