find_package(Catch2 3 REQUIRED)

set(TEST_SOURCES
    main.cpp
    tests.h
    tests.cpp
    fbx_info.h
    fbx_info.cpp
)

include_directories(${cfbx_SOURCE_DIR}/src)

add_executable(tests ${TEST_SOURCES} ${TARGET_SOURCES})
target_link_libraries(tests PRIVATE cfbx Catch2::Catch2WithMain)
set_property(TARGET tests PROPERTY CXX_STANDARD 20)
