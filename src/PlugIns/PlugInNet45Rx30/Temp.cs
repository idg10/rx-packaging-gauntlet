using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlugInNet45Rx30
{
    internal class Temp
    {
        public void Foo()
        {
            IObservable<int> observable = Observable.Range(1, 10).Select(x => x * 2).Where(x => x > 5);
            Control c = null!;

            observable.ObserveOn(c).Subscribe(result => 
            {

                Console.WriteLine(result);
            });
        }
    }
}
