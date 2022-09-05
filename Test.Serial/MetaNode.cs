using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using SerialPortLib;
using NLog;


namespace MetaLib
{
    public class MetaNode
    {
        #region Private fields

        private MetaNodeController controller;

        #endregion

        #region Public fields and events

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public byte Id { get; /*protected*/ set; }
        #endregion


        internal void SetController(MetaNodeController controller)
        {
            this.controller = controller;
        }
    }
}
