using Search.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;

namespace Search.Core.App_Start
{
    /// <summary>
    /// Registers the type mappings with Umbraco IoC.
    /// </summary>
    static class Registrations
    {
        /// <summary>Registers the type mappings with Umbraco IoC.</summary>
        public static void Register(Composition composition)
        {
            composition.Register<SearchService>();
        }
    }
}
