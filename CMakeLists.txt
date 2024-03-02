
cmake_minimum_required(VERSION 3.26)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# set(CMAKE_CXX_COMPILER clang++)
# set(CMAKE_CXX_STANDARD 23)
# set(CMAKE_CXX_FLAGS -Wall -Wextra)

# set(CMAKE_C_COMPILER clang)
# set(CMAKE_C_STANDARD 17)
# set(CMAKE_C_FLAGS -Wall -Wextra)

project(mavre)

include(FetchContent)
FetchContent_Populate(mavlink
  GIT_REPOSITORY https://github.com/mavlink/c_library_v2
  GIT_TAG master)

add_custom_target(run
  DEPENDS ${CMAKE_SOURCE_DIR}/mavre.cs
  COMMAND mono-csc ${CMAKE_SOURCE_DIR}/mavre.cs
              -out:${CMAKE_BINARY_DIR}/mavre.exe
              -r:System.Windows.Forms.dll
              -r:System.Xml.Linq.dll
              -debug:full
  COMMAND ./mavre.exe ${CMAKE_BINARY_DIR}/mavlink-src/message_definitions/all.xml)
