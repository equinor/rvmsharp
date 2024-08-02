#include "manager.h"
#include <fbxsdk.h>

CFbxManager manager_create()
{
    auto manager = FbxManager::Create();
    return static_cast<CFbxManager>(manager);
}

void manager_destroy(CFbxManager manager)
{
    if (manager == nullptr)
        return;

    auto fbxManager = static_cast<FbxManager*>(manager);
    fbxManager->Destroy();
}

bool assert_fbxsdk_version_newer_or_equal_than(const char* minFbxVersion)
{
    auto min = std::string(minFbxVersion);
    auto version = std::string(FBXSDK_VERSION);
    auto compareResult = version.compare(min);

    return version.compare(min) >= 0;
}
