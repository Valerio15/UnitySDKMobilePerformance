import Foundation
import os.log
import GPUUtilization

@objc public class PerformanceMonitor: NSObject {
    static let shared = PerformanceMonitor()

    public var timer: Timer?
    public var cpuUsage: Double = 0.0
    public var memoryUsage: Double = 0.0
    public var gpuUsage: Double = 0.0

    private override init() {
        super.init()
    }

    @objc private func trackUsage() {
        cpuUsage = getCPUUsage()
        memoryUsage = getMemoryUsage()
        gpuUsage = getGPUUsage()
    }

    private func getCPUUsage() -> Double {
        var kr: kern_return_t
        var task_info_count = mach_msg_type_number_t(TASK_INFO_MAX)
        var tinfo = task_basic_info_data_t()

        kr = withUnsafeMutablePointer(to: &tinfo) {
            $0.withMemoryRebound(to: integer_t.self, capacity: 1) {
                task_info(mach_task_self_, task_flavor_t(TASK_BASIC_INFO), $0, &task_info_count)
            }
        }

        guard kr == KERN_SUCCESS else {
            return -1
        }

        var thread_list: thread_act_array_t? = nil
        var thread_count = mach_msg_type_number_t()
        var thinfo = thread_basic_info()

        kr = task_threads(mach_task_self_, &thread_list, &thread_count)
        guard kr == KERN_SUCCESS else {
            return -1
        }

        var tot_cpu: Double = 0

        if let thread_list = thread_list {
            for j in 0..<Int(thread_count) {
                var thread_info_count = mach_msg_type_number_t(THREAD_INFO_MAX)
                kr = withUnsafeMutablePointer(to: &thinfo) {
                    $0.withMemoryRebound(to: integer_t.self, capacity: 1) {
                        thread_info(thread_list[j], thread_flavor_t(THREAD_BASIC_INFO), $0, &thread_info_count)
                    }
                }

                guard kr == KERN_SUCCESS else {
                    return -1
                }

                if thinfo.flags != TH_FLAGS_IDLE {
                    tot_cpu += Double(thinfo.cpu_usage) / Double(TH_USAGE_SCALE) * 100.0
                }
            }

            vm_deallocate(mach_task_self_, vm_address_t(bitPattern: thread_list), vm_size_t(thread_count) * UInt(MemoryLayout<thread_act_t>.size))
        }

        return tot_cpu
    }

    private func getMemoryUsage() -> Double {
        var task_info_count = mach_msg_type_number_t(TASK_INFO_MAX)
        var tinfo = task_basic_info()

        let result: kern_return_t = withUnsafeMutablePointer(to: &tinfo) {
            $0.withMemoryRebound(to: integer_t.self, capacity: 1) {
                task_info(mach_task_self_, task_flavor_t(TASK_BASIC_INFO), $0, &task_info_count)
            }
        }

        if result == KERN_SUCCESS {
            let usedBytes = tinfo.resident_size
            return Double(usedBytes) / 1048576.0 // Convert to MB
        } else {
            return -1.0
        }
    }

    private func getGPUUsage() -> Double {
        return Double(GPUUtilization.gpuUsage)
    }
}

var persistentCString: UnsafeMutablePointer<CChar>? = nil

@_cdecl("StartTracking")
public func startTracking() {
    os_log("StartTracking called", type: .info)
    PerformanceMonitor.shared.startTrackingInternal()
}

@_cdecl("StopTracking")
public func stopTracking() -> UnsafePointer<CChar>? {
    os_log("StopTracking called", type: .info)
    persistentCString?.deallocate()
    let usageData = "CPU: \(PerformanceMonitor.shared.cpuUsage)%\nMemory: \(PerformanceMonitor.shared.memoryUsage) MB\nGPU: \(PerformanceMonitor.shared.gpuUsage)%"
    os_log("Usage Data: %@", type: .info, usageData)
    persistentCString = strdup((usageData as NSString).utf8String)
    return UnsafePointer(persistentCString)
}

extension PerformanceMonitor {
    fileprivate func startTrackingInternal() {
        timer?.invalidate() // Ensure no existing timer is running
        timer = Timer.scheduledTimer(timeInterval: 1.0, target: self, selector: #selector(trackUsage), userInfo: nil, repeats: true)
        os_log("Timer started", type: .info)
    }

    fileprivate func stopTrackingInternal() -> UnsafePointer<CChar>? {
        timer?.invalidate()
        os_log("Timer stopped", type: .info)
        return stopTracking()
    }
}
