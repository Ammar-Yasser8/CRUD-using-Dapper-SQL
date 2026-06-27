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

        // Transaction in Dapper
        // (Take 2000 from employee with Id 1 and add it to employee with Id 2)
        [HttpPost("transfer")]
        public async Task<ActionResult> TransferSalary(int fromEmployeeId, int toEmployeeId, decimal amount)
        {
            using var transaction = _connection.BeginTransaction();
            try
            {
                var deductSql = "UPDATE Employees SET Salary = Salary - @Amount WHERE Id = @Id";
                var addSql = "UPDATE Employees SET Salary = Salary + @Amount WHERE Id = @Id";
                await _connection.ExecuteAsync(deductSql, new { Amount = amount, Id = fromEmployeeId }, transaction);
                await _connection.ExecuteAsync(addSql, new { Amount = amount, Id = toEmployeeId }, transaction);
                transaction.Commit();
                return Ok("Transfer successful");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest($"Transfer failed: {ex.Message}");
            }
        }
    }
}
