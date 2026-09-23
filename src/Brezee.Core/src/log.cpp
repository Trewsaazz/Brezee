#include <brezee/core/log.h>

#include <memory>
#include <mutex>
#include <utility>

namespace brezee::core {

namespace {

std::mutex sink_mutex;
std::shared_ptr<const LogSink> current_sink;

} // namespace

void set_log_sink(LogSink sink)
{
    auto next = sink ? std::make_shared<const LogSink>(std::move(sink)) : nullptr;

    std::lock_guard lock(sink_mutex);
    current_sink = std::move(next);
}

void log(LogLevel level, std::string_view category, std::string_view message)
{
    std::shared_ptr<const LogSink> sink;
    {
        std::lock_guard lock(sink_mutex);
        sink = current_sink;
    }

    // Called outside the lock so a sink may log or swap sinks without deadlocking.
    if (sink)
        (*sink)(level, category, message);
}

} // namespace brezee::core
