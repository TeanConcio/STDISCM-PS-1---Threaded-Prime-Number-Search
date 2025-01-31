using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STDISCM_PS_1___Threaded_Prime_Number_Search
{
    internal class PrimeSearchThread
    {
        public int ID { get; set; }

        Thread Thread { get; set; }



        public PrimeSearchThread(int id)
        {
            this.ID = id;

            Thread = new Thread(Run);
        }

        public virtual void Run()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            Thread.Start();
        }

        public void Join()
        {
            Thread.Join();
        }
    }
}
