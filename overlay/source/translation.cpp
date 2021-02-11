extern "C" {
#include <switch.h>
#include <cstring>
#define MIN_LNG_SIZE sizeof(u64) + 1
void GetSystemLanguage(char* lng) {
    u64 s_textLanguageCode = 0;
    Result rc = setInitialize();
    if (R_SUCCEEDED(rc)) rc = setGetSystemLanguage(&s_textLanguageCode);
    setExit();
    memset(lng, 0x00, MIN_LNG_SIZE);
    memcpy(lng, (void*)&s_textLanguageCode, sizeof(s_textLanguageCode));
}
}

#include <switch.h>
#include <tesla.hpp>
#include <translation.h>
#include <filesystem>
#include <fstream>
#include <streambuf>

#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpessimizing-move"
#include <json.hpp>
#pragma GCC diagnostic pop

Translator& Translator::global() {
    static Translator glTr;
    return glTr;
}

std::string Translator::translate(const std::string& source, const std::string& context) {
    return global().get(source, context);
}

void Translator::loadSystem() {
    char lng[MIN_LNG_SIZE];
    GetSystemLanguage(lng);
    loadCustom(lng);
}

void Translator::loadCustom(const std::string& lng) {
    if (language == lng) {
        return;
    }
    language = lng;
    strings.clear();

    tsl::hlp::doWithSDCardHandle([this] {
        std::filesystem::path lng_file{"sdmc:/switch/.overlays/emuiibo/lng_" + language + ".json"};
        if (!std::filesystem::exists(lng_file)) {
            return;
        }

        std::ifstream json_file(lng_file);
        const std::string json_string((std::istreambuf_iterator<char>(json_file)),
                                std::istreambuf_iterator<char>());
        auto json_all = json::JSON::Load(json_string);
        for (auto json_string: json_all["strings"].ArrayRange()) {
            const auto context = json_string["context"].ToString();
            const auto source = json_string["source"].ToString();
            const auto translation = json_string["translation"].ToString();
            strings[context][source] = translation;
        }
    });
}

std::string Translator::get(const std::string& source, const std::string& context) const {
    if (strings.count(context) == 0) {
        return source;
    }
    if (strings.at(context).count(source) == 0) {
        return source;
    }
    if (strings.at(context).at(source).empty()) {
        return source;
    }
    return strings.at(context).at(source);
}
