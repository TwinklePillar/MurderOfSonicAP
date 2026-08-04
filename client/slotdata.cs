using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace tmosthap {
	class SlotData {
		public static int version;
		public static bool carRando;
		public static List<string> entrances;

		public SlotData(Dictionary<string, object> slot) {
			version = (int)(long)slot["version"];
			carRando = (bool)slot["car_rando"];
			entrances = new List<string>();
			foreach (string key in (JArray)slot["entrances"]) {
				entrances.Add(key);
			}
		}

		public SlotData() {
			version = -1;
			carRando = false;
			entrances = new List<string>();
		}
	}
}