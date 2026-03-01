using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AttendanceTracker.Data.Services;

public static class ServiceCollectionExtensions
{
    public static void AddDataServices(this IServiceCollection collection)
    {
        // TODO: add connection string from context
        collection.AddDbContext<DatabaseContext>();
    }
}
