using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace ConsoleDomainsTestApp
{
	class DomainSet
	{
		#region Public Methods

		public DomainSet(string pathToTxtDb)
		{
			if (string.IsNullOrEmpty(pathToTxtDb))
			{
				throw new ArgumentException("pathToTxtDb should not be null or empty");
			}

			using (TextReader reader = File.OpenText(pathToTxtDb))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					_dbHash.Add(line);
				}
			}
		}

		public IList<string> GetRandomSubSet(uint listCount)
		{
			if (listCount > _dbHash.Count)
			{
				throw new ArgumentException("listCount should be lower than the DomainList count");
			}

			var dbList = _dbHash.ToList();

			var subSet = new List<string>();

			for (int i = 0; i < listCount; i++)
			{
				subSet.Add(dbList[Rnd.Next(dbList.Count)]);
			}

			return subSet;
		}

		public bool IsDomainMatcheBySubDomain(string domainName)
		{
			if (string.IsNullOrEmpty(domainName))
			{
				throw new ArgumentException("domainName should not be null or empty");
			}

			var domainParts = domainName.Split(new char[] { '.' });

			var sb = new StringBuilder();

			for (int i = domainParts.Length - 1; i >= 0; i--)
			{
				sb.Insert(0, domainParts[i]);

				if (_dbHash.Contains(sb.ToString()))
				{
					return true;
				}
				sb.Insert(0, '.');
			}
			return false;
		}

		#endregion

		#region Public Properties

		public int DomainsCount
		{
			get { return _dbHash.Count; }
		}

		#endregion

		#region Private Fields

		private HashSet<string> _dbHash = new HashSet<string>();
		private static readonly Random Rnd = new Random(DateTime.Now.Millisecond);

		#endregion
	}

	class Program
	{
		static void Main(string[] args)
		{
			// Path to textDB should be placed below:
			var setObj = new DomainSet(@"..\..\domains.csv");

			var rndList = setObj.GetRandomSubSet(10000);

			var matchedList0 = new List<string>();
			var matchedList1 = new List<string>();

			var locker = new object();

			var sw = new Stopwatch();

			sw.Start();

			Task mainTask = Task.Run(() =>
			{
				var tf = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.None);

				tf.StartNew(() =>
					{
						for (int i = 0; i < (int) (rndList.Count / 4); i++)
						{
							if (setObj.IsDomainMatcheBySubDomain(rndList[i]))
							{
								lock (locker)
								{
									matchedList0.Add(rndList[i]);
								}
							}
						}
					});
				tf.StartNew(() =>
					{
						for (int i = (int) (rndList.Count / 4) + 1; i < (int) (rndList.Count / 2); i++)
						{
							if (setObj.IsDomainMatcheBySubDomain(rndList[i]))
							{
								lock (locker)
								{
									matchedList0.Add(rndList[i]);
								}
							}
						}
					});
				tf.StartNew(() =>
					{
						for (int i = (int) (rndList.Count / 2) + 1; i < (int) (rndList.Count * 3 / 4); i++)
						{
							if (setObj.IsDomainMatcheBySubDomain(rndList[i]))
							{
								lock (locker)
								{
									matchedList0.Add(rndList[i]);
								}
							}
						}
					});
				tf.StartNew(() =>
				{
					for (int i = (int) (rndList.Count * 3 / 4) + 1; i < (int) (rndList.Count); i++)
					{
						if (setObj.IsDomainMatcheBySubDomain(rndList[i]))
						{
							lock (locker)
							{
								matchedList0.Add(rndList[i]);
							}
						}
					}
				});
			});

			mainTask.Wait();

			sw.Stop();

			Console.WriteLine("Threading");
			Console.WriteLine("Domain search time for cycle of {0} random entries: {1}\r\n", rndList.Count, sw.Elapsed);

			sw.Reset();
			sw.Start();

			foreach (var domain in rndList)
			{
				if (setObj.IsDomainMatcheBySubDomain(domain))
				{
					matchedList1.Add(domain);
				}
			}
			sw.Stop();

			Console.WriteLine("Non-Threading");
			Console.WriteLine("Domain search time for cycle of {0} random entries: {1}\r\n", rndList.Count, sw.Elapsed);
			Console.WriteLine("Please press Enter to exit.");
			Console.ReadLine();
		}
	}
}
