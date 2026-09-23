#pragma once

#include <string_view>

namespace brezee::core {

// Semantic version of the Brezee core library, UTF-8 encoded.
[[nodiscard]] std::string_view version() noexcept;

} // namespace brezee::core
