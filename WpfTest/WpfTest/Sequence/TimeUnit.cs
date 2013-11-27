using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIDaqController {
	//時間の種類
	public enum TimeUnit {
		s,
		ms,
		us
	}
	public static class TimeUnitExt {
		public static double getTime(this TimeUnit unit) {
			if (unit == TimeUnit.s) {
				return 1.0;
			} else if (unit == TimeUnit.ms) {
				return 1.0e-3;
			} else if (unit == TimeUnit.us) {
				return 1.0e-6;
			}
			return 1.0;
		}
	}
}
