#!/bin/sh

cli_arg=$@

project_name=prototype_server

alt_cover_path=~/.nuget/packages/altcover/5.2.667/tools/net45/AltCover.exe
report_gen_path=~/.nuget/packages/reportgenerator/4.1.0/tools/net47/ReportGenerator.exe

type_filter="DesperateDevs|Libs"
path_filter="Libs"
assembly_filter="Unity"

result_dir="bin/Debug"
report_dir="${result_dir}/Reports"
coverage_output_dir="${result_dir}/__Instrumented"

xml_report_file="${report_dir}/coverage.xml"

rm -rf ${result_dir}/Reports
msbuild /property:Configuration=Debug /verbosity:minimal ${project_name}.Specs.csproj

if [[ ${cli_arg} = "--coverage" ]]; then
    mono ${alt_cover_path} --xmlReport=${xml_report_file} --outputDirectory=${coverage_output_dir} --inputDirectory=${result_dir} --inplace --typeFilter=${type_filter} --pathFilter=${path_filter} --assemblyFilter=${assembly_filter} --assemblyExcludeFilter="nunit|Fluent|Specs" --opencover --save
    rm -rf ${coverage_output_dir}
fi

if [[ ${cli_arg} = "--coverage" ]] || [[ ${cli_arg} != "" ]]; then
    mono ${result_dir}/netcoreapp2.2/${project_name}.Specs.dll ${cli_arg}
else
    mono ${result_dir}/netcoreapp2.2/${project_name}.Specs.dll
fi

if [[ ${cli_arg} = "--coverage" ]]; then
    mono ${alt_cover_path} runner --collect --recorderDirectory=${result_dir}
    mono ${report_gen_path} "-reports:${xml_report_file}" "-targetdir:${report_dir}" "-reporttypes:Html;Badges"
fi