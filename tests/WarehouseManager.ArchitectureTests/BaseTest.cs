using System.Reflection;
using WarehouseManager.Api.Abstract;

namespace WarehouseManager.ArchitectureTests;

public abstract class BaseTest
{
    protected static readonly Assembly ApiAssembly = typeof(IEndpoint).Assembly;
}
