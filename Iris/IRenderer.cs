using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public interface IRenderer
    {
        void DrawFrame(UInt16[] frameBuffer);
    }
}
