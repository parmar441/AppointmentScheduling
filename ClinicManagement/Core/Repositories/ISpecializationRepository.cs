using System.Collections.Generic;
using ClinicManagement.Core.Models;

namespace ClinicManagement.Core.Repositories
{
    public interface ISpecializationRepository
    {
        IEnumerable<Specialization> GetSpecializations();
        Specialization GetSpecializationName(int id);
    }
}
