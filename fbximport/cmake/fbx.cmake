# Based on FBX SDK samples CMakeLists.txt
# Notes:
# - On linux it might be difficult to compile with static linking due to different versions in stdc++ library, recommend setting FBX_SHARED to 1
#
# Changes:
# - Removed everything concerning FBX samples
# - Commented out reset of FBX_ flags
# - Added debug/release switch based on CMAKE_BUILD_TYPE

# GUSH: do not reset flags
#set(FBX_SHARED)         # can be set at command line with -DFBX_SHARED=1
#set(FBX_STATIC_RTL)     # can be set at command line with -DFBX_STATIC_RTL=1 (use static MSVCRT (/MT), otherwise use dynamic MSVCRT (/MD))
#set(FBX_VARIANT)        # can be set at command line with -DFBX_VARIANT=debug or release (Unix only)
#set(FBX_ARCH)           # can be set at command line with -DFBX_ARCH=x64 or x86 (Unix only)


if(FBX_SHARED AND FBX_STATIC_RTL)
    set(FBX_STATIC_RTL)
    message("\nBoth FBX_SHARED and FBX_STATIC_RTL have been defined. They are mutually exclusive, considering FBX_SHARED only.")
endif()

# GUSH: define FBX variant based on CMAKE_BUILD_TYPE
#if(NOT FBX_VARIANT)
#    set(FBX_VARIANT "debug")
#endif()
if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    set(FBX_VARIANT "debug")
else()
    set(FBX_VARIANT "release")
endif()

set(FBX_DEBUG)
IF (FBX_VARIANT MATCHES "debug")
    set(FBX_DEBUG 1)
endif()

if(NOT FBX_ARCH)
    set(FBX_ARCH "x64")
    if(WIN32 AND NOT CMAKE_CL_64)
        set(FBX_ARCH "x86")
    endif()
endif()

if(WIN32)
    set(CMAKE_USE_RELATIVE_PATHS 1)
    set(LIB_EXTENSION ".lib")
ELSE(WIN32)
    set(LIB_EXTENSION ".a")
endif(WIN32)

set(FBX_SDK libfbxsdk${LIB_EXTENSION})
if(WIN32)
    if(CMAKE_CONFIGURATION_TYPES)
        set(CMAKE_CONFIGURATION_TYPES Debug Release RelWithDebInfo)
        set(CMAKE_CONFIGURATION_TYPES "${CMAKE_CONFIGURATION_TYPES}" CACHE STRING "Reset the configurations to what we need" FORCE)
    endif()
    
    set(FBX_VARIANT "$(Configuration)")
    
    if(MSVC_VERSION GREATER 1899 AND MSVC_VERSION LESS 1911)
        set(FBX_COMPILER "vs2015")
    elseif(MSVC_VERSION GREATER 1910 AND MSVC_VERSION LESS 1920)
        set(FBX_COMPILER "vs2017")
    elseif(MSVC_VERSION GREATER 1919)
        set(FBX_COMPILER "vs2019")
    endif()
    set(FBX_TARGET_LIBS_PATH "${FBX_ROOT}/lib/${FBX_COMPILER}/${FBX_ARCH}/${FBX_VARIANT}")
    set(FBX_SDK_ABS ${FBX_TARGET_LIBS_PATH}/${FBX_SDK})
    set(FBX_REQUIRED_LIBS_DEPENDENCY ${FBX_SDK_ABS})
    if(NOT FBX_SHARED)
        if(FBX_STATIC_RTL)
            set(FBX_CC_RTL "/MT")
            set(FBX_CC_RTLd "/MTd")
            set(FBX_RTL_SUFFX "-mt")
        else()
            set(FBX_CC_RTL "/MD")
            set(FBX_CC_RTLd "/MDd")
            set(FBX_RTL_SUFFX "-md")
        endif()
        set(FBX_REQUIRED_LIBS_DEPENDENCY
            ${FBX_TARGET_LIBS_PATH}/libfbxsdk${FBX_RTL_SUFFX}${LIB_EXTENSION} 
            ${FBX_TARGET_LIBS_PATH}/libxml2${FBX_RTL_SUFFX}${LIB_EXTENSION} 
            ${FBX_TARGET_LIBS_PATH}/zlib${FBX_RTL_SUFFX}${LIB_EXTENSION})
    endif()
else()
    message("Trying to detect which compiler version is used")
    message("CMAKE_CXX_COMPILER: ${CMAKE_CXX_COMPILER}")
    execute_process(COMMAND ${CMAKE_CXX_COMPILER} ARGS --version OUTPUT_VARIABLE CMAKE_CXX_COMPILER_VERSION)
    message("OUTPUT_VARIABLE: ${OUTPUT_VARIABLE}, CMAKE_CXX_COMPILER_VERSION: ${CMAKE_CXX_COMPILER_VERSION}")
    if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        message("Detected Clang ${CMAKE_CXX_COMPILER_VERSION}")
        set(FBX_COMPILER "clang")
        set(FBX_CLANG 1)
    else()
        set(FBX_COMPILER "gcc")
        if(CMAKE_CXX_COMPILER_VERSION MATCHES "([4-9]|[1-9][0-9])\.[0-9]+\.[0-9]+")
            message( "Detected GCC >= 4.0" )
        else()
            message(FATAL_ERROR  "Detected " ${GCC_PREFIX} " only GCC 4.x and higher supported")
        endif()
    endif()

    if(APPLE)
        set(FBX_TARGET_LIBS_PATH "${FBX_ROOT}/lib/${FBX_COMPILER}/${FBX_VARIANT}")
        if(FBX_COMPILER STREQUAL "gcc")
            set(FBX_TARGET_LIBS_PATH "${FBX_ROOT}/lib/${FBX_COMPILER}/ub/${FBX_VARIANT}")
        endif()
    elseif(LINUX)
        set(FBX_TARGET_LIBS_PATH "${FBX_ROOT}/lib/${FBX_VARIANT}")
    endif()
    set(FBX_EXTRA_LIBS_PATH ${FBX_TARGET_LIBS_PATH}/lib)
    set(FBX_SDK_ABS ${FBX_EXTRA_LIBS_PATH}fbxsdk${LIB_EXTENSION})

    if(APPLE)
        set(FBX_REQUIRED_LIBS_DEPENDENCY ${FBX_SDK_ABS} z xml2)
    else()
        set(FBX_REQUIRED_LIBS_DEPENDENCY ${FBX_SDK_ABS} xml2)
    endif()
endif()
