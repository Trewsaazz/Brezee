#include <brezee/core/version.h>

#include <doctest.h>

#include <algorithm>
#include <cctype>
#include <string>
#include <vector>

namespace {

std::vector<std::string> split(std::string_view text, char separator)
{
    std::vector<std::string> parts;
    std::string current;
    for (char c : text)
    {
        if (c == separator)
        {
            parts.push_back(current);
            current.clear();
        }
        else
        {
            current.push_back(c);
        }
    }
    parts.push_back(current);
    return parts;
}

bool is_number(const std::string& text)
{
    return !text.empty()
        && std::all_of(text.begin(), text.end(), [](unsigned char c) { return std::isdigit(c) != 0; });
}

} // namespace

TEST_CASE("version is MAJOR.MINOR.PATCH")
{
    const auto parts = split(brezee::core::version(), '.');

    REQUIRE(parts.size() == 3);
    for (const auto& part : parts)
        CHECK(is_number(part));
}

TEST_CASE("version is stable across calls")
{
    CHECK(brezee::core::version() == brezee::core::version());
}
