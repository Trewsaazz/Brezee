#include "client.h"

#include <brezee/core/log.h>

#include <mutex>
#include <string>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#else
#include <dlfcn.h>
#endif

namespace brezee::core::firebird {

namespace {

using GetMasterInterface = Firebird::IMaster* (*)();

std::mutex client_mutex;

// Deliberately never deleted: fbclient stays loaded until the process exits, and releasing its
// interfaces from a static destructor could run after fbclient has shut itself down.
Client* loaded_client = nullptr;

#ifdef _WIN32

std::wstring to_wide(const std::string& utf8)
{
    if (utf8.empty())
        return {};

    const int length = MultiByteToWideChar(CP_UTF8, 0, utf8.data(), static_cast<int>(utf8.size()), nullptr, 0);
    std::wstring wide(static_cast<std::size_t>(length), L'\0');
    MultiByteToWideChar(CP_UTF8, 0, utf8.data(), static_cast<int>(utf8.size()), wide.data(), length);
    return wide;
}

GetMasterInterface load_entry_point(const std::string& library)
{
    const auto path = to_wide(library);
    const bool has_directory = path.find_first_of(L"\\/") != std::wstring::npos;

    // With a full path, resolve fbclient's own dependencies from its folder too.
    HMODULE module = LoadLibraryExW(path.c_str(), nullptr, has_directory ? LOAD_WITH_ALTERED_SEARCH_PATH : 0);
    if (!module)
        throw Error(ErrorKind::Internal,
            "The Firebird client library could not be loaded (" + library + ", error " + std::to_string(GetLastError()) + ").");

    auto entry = reinterpret_cast<GetMasterInterface>(
        reinterpret_cast<void*>(GetProcAddress(module, "fb_get_master_interface")));
    if (!entry)
        throw Error(ErrorKind::Internal, library + " is not a Firebird 3.0 or newer client library.");

    return entry;
}

#else

GetMasterInterface load_entry_point(const std::string& library)
{
    void* module = dlopen(library.c_str(), RTLD_NOW);
    if (!module)
        throw Error(ErrorKind::Internal,
            "The Firebird client library could not be loaded (" + library + ": " + dlerror() + ").");

    auto entry = reinterpret_cast<GetMasterInterface>(dlsym(module, "fb_get_master_interface"));
    if (!entry)
        throw Error(ErrorKind::Internal, library + " is not a Firebird 3.0 or newer client library.");

    return entry;
}

#endif

} // namespace

Client::Client(std::string library, Firebird::IMaster* master)
    : library_(std::move(library))
    , master_(master)
    , util_(master->getUtilInterface())
    , provider_(master->getDispatcher())
{
}

void Client::load(const std::string& library)
{
    std::lock_guard lock(client_mutex);
    if (loaded_client)
        return;

    auto entry = load_entry_point(library);
    loaded_client = new Client(library, entry());
}

Client& Client::get()
{
    std::lock_guard lock(client_mutex);
    if (!loaded_client)
        throw Error(ErrorKind::Internal, "The Firebird client is not loaded. Call brezee::core::initialize() first.");

    return *loaded_client;
}

Status::Status()
    : wrapper_(Client::get().master()->getStatus())
{
}

Status::~Status()
{
    wrapper_.dispose();
}

void throw_error(const Firebird::FbException& error, ErrorKind kind, std::string_view context)
{
    char buffer[2048] = {};
    Client::get().util()->formatStatus(buffer, sizeof(buffer), error.getStatus());

    std::string message(buffer);
    log(LogLevel::Error, "Firebird", std::string(context) + ": " + message);

    throw Error(kind, message);
}

} // namespace brezee::core::firebird
