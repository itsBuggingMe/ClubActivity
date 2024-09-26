using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClubActivity
{
    public interface IFunction
    {
        Choice Accept(Round[] rounds);
    }
}
