import Darwin

public class PerformanceTimer {
    let machTimebaseInfo: mach_timebase_info
    var lastCounter: UInt64

    public init() {
        var timeInfo = mach_timebase_info()
        mach_timebase_info(&timeInfo)	

        self.machTimebaseInfo = timeInfo		
        self.lastCounter = mach_absolute_time()
    }

    public func start() {
        self.lastCounter = mach_absolute_time()
    }

    public func stop(_ message: String) -> Double {
        let currentCounter = mach_absolute_time()
        let elapsed = currentCounter - self.lastCounter		
        let nanoSeconds = elapsed * UInt64(self.machTimebaseInfo.numer) / UInt64(self.machTimebaseInfo.denom)		
        let milliSeconds = Double(nanoSeconds) / 1_000_000	

        print("\(message) - \(milliSeconds)")

        return milliSeconds
    }
}