#pragma once

// Internal to Brezee.Core: runs one SQL statement on an attachment and reads its results.

#include <brezee/core/query.h>

#include <firebird/Interface.h>

#include <string_view>

namespace brezee::core::firebird {

// Prepares and runs a statement in its own transaction, committed when it succeeds.
// SELECTs (and selectable procedures) return their rows; other statements return no columns.
// Throws Error(Database) on failure.
QueryResult run_statement(Firebird::IAttachment* attachment, std::string_view sql,
    const Parameters& parameters, const QueryOptions& options);

} // namespace brezee::core::firebird
