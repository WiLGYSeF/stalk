using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wilgysef.Stalk.Application.Services;

public interface IOutputPathService
{
    string GetOutputPath(string path);
}
