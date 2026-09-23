#pragma once

namespace brezee::core {

// Prepares the core for use. Call once at startup, after attaching a log sink.
// Throws brezee::core::Error on failure.
void initialize();

} // namespace brezee::core
