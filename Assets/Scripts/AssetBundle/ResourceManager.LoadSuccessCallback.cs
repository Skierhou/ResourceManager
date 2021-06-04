using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlay
{
    public partial class ResourceManager
    {
        private sealed class LoadSuccessCallback
        {
            public Action<UnityEngine.Object, object> callback;
            public object userData;

            public LoadSuccessCallback(Action<UnityEngine.Object, object> callback, object userData) 
            {
                this.callback = callback;
                this.userData = userData;
            }
        }
    }
}
