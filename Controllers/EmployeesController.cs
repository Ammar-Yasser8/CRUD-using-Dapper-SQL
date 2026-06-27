using Dapper;
using EmployeeApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace EmployeeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IDbConnection _connection;

        public EmployeesController(IDbConnection connection)
        {
            _connection = connection;
        }
        

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            var employees = await _connection.QueryAsync<Employee>("SELECT * FROM Employees");
            return Ok(employees);
        }
        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var sql = "SELECT * FROM Employees WHERE Id = @Id";
            var employee = await _connection.QueryFirstOrDefaultAsync<Employee>(sql, new { Id = id });
               
            if (employee is null)
                return NotFound();
            return Ok(employee);
        }
        [HttpPost]
        public async Task<ActionResult<Employee>>CreateEmployee(Employee employee)
        {
            var sql = "INSERT INTO Employees (FullName, Email, Salary, HireDate)" +
                " VALUES (@FullName, @Email, @Salary, @HireDate); " +
                "" + "SELECT CAST(SCOPE_IDENTITY() as int)";
            var id = await _connection.QuerySingleAsync<int>(sql, employee);
            employee.Id = id;
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            const string sql = """
                   UPDATE Employees
                   SET
                       FullName = @FullName,
                       Email = @Email,
                       Salary = @Salary,
                       HireDate = @HireDate
                   WHERE Id = @Id;
                   """;
            employee.Id = id;

            var rowsAffected = await _connection.ExecuteAsync(sql, employee);

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<Employee>> DeleteEmployee(int id)
        {
            var sql = "DELETE FROM Employees " +
                "WHERE Id =@Id";
            var rowsAffected = await _connection.ExecuteAsync(sql, new { Id = id });
            if (rowsAffected == 0)
                return NotFound();
            return NoContent();
        }

        // Get Min and Max Salary (Execute multiple queries in one go)
        [HttpGet("salary-range")]
        public async Task<ActionResult> GetSalaryRange()
        {
            var sql = "SELECT MIN(Salary) AS MinSalary, MAX(Salary) AS MaxSalary FROM Employees";
            var result = await _connection.QueryFirstOrDefaultAsync(sql);
            return Ok(result);
        }
    }
}
