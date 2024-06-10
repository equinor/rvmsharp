set(SOURCES
    main.cpp
    tests.h
    tests.cpp
    fbx_info.h
    fbx_info.cpp
)

include(catch2)
include_directories(${cfbx_SOURCE_DIR}/src)

add_executable(tests ${SOURCES})
target_link_libraries(tests PRIVATE cfbx Catch2::Catch2WithMain)
set_property(TARGET tests PROPERTY CXX_STANDARD 20)
