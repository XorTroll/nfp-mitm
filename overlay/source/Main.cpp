
#define TESLA_INIT_IMPL
#include <emuiibo.hpp>
#include <libtesla_ext.hpp>
#include <dirent.h>
#include <translation.h>

namespace {

    bool g_Initialized = false;
    bool g_NeedsUpdateCategoryList = false;
    bool g_NeedsUpdateMainGui = false;
    bool g_CurrentApplicationIntercepted = false;
    char g_VirtualAmiiboDirectory[FS_MAX_PATH];
    emu::Version g_Version;

    char g_ActiveVirtualAmiiboPath[FS_MAX_PATH];
    emu::VirtualAmiiboData g_ActiveVirtualAmiiboData;

    inline bool IsActiveAmiiboValid() {
        return strlen(g_ActiveVirtualAmiiboPath) > 0;
    }

    inline void UpdateActiveAmiibo() {
        emu::GetActiveVirtualAmiibo(&g_ActiveVirtualAmiiboData, g_ActiveVirtualAmiiboPath, sizeof(g_ActiveVirtualAmiiboPath));
    }

    // Returns true if the value changed
    inline bool UpdateCurrentApplicationIntercepted() {
        bool ret = emu::IsCurrentApplicationIdIntercepted();
        if(ret != g_CurrentApplicationIntercepted) {
            g_CurrentApplicationIntercepted = ret;
            return true;
        }
        return false;
    }

    inline std::string MakeActiveAmiiboText() {
        if(IsActiveAmiiboValid()) {
            return TR("Active virtual amiibo: ") + g_ActiveVirtualAmiiboData.name;
        }
        return TR("No active virtual amiibo");
    }

    inline std::string MakeTitleText() {
        if(!g_Initialized) {
            return "emuiibo";
        }
        return "emuiibo v" + std::to_string(g_Version.major) + "." + std::to_string(g_Version.minor) + "." + std::to_string(g_Version.micro) + " (" + (g_Version.dev_build ? "dev" : "release") + ")";
    }

    inline std::string MakeStatusText() {
        if(!g_Initialized) {
            return TR("emuiibo was not accessed.");
        }
        std::string msg = TR("Emulation: ");
        auto e_status = emu::GetEmulationStatus();
        switch(e_status) {
            case emu::EmulationStatus::On: {
                msg += TR("on") + "\n";
                auto v_status = emu::GetActiveVirtualAmiiboStatus();
                switch(v_status) {
                    case emu::VirtualAmiiboStatus::Invalid: {
                        msg += TR("No active virtual amiibo.");
                        break;
                    }
                    case emu::VirtualAmiiboStatus::Connected: {
                        msg += TR("Virtual amiibo: ");
                        msg += g_ActiveVirtualAmiiboData.name;
                        msg += TR(" (connected - select to disconnect)");
                        break;
                    }
                    case emu::VirtualAmiiboStatus::Disconnected: {
                        msg += TR("Virtual amiibo: ");
                        msg += g_ActiveVirtualAmiiboData.name;
                        msg += TR(" (disconnected - select to connect)");
                        break;
                    }
                }
                msg += "\n";
                if(g_CurrentApplicationIntercepted) {
                    msg += TR("Current game is being intercepted by emuiibo.");
                }
                else {
                    msg += TR("Current game is not being intercepted.");
                }
                break;
            }
            case emu::EmulationStatus::Off: {
                msg += TR("off");
                break;
            }
        }
        return msg;
    }

}

class AmiiboList : public tsl::Gui {

    private:
        tsl::elm::CustomOverlayFrame *root_frame;
        tsl::elm::BigCategoryHeader *selected_header;
        tsl::elm::CategoryHeader *count_header;
        tsl::elm::List *list;
        tsl::elm::List *header_list;
        std::string amiibo_path;

    public:
        AmiiboList(const std::string &path) : root_frame(new tsl::elm::CustomOverlayFrame(MakeTitleText(), MakeStatusText())), amiibo_path(path) {}

        bool OnItemClick(u64 keys, const std::string &path) {
            if(keys & KEY_A) {
                char amiibo_path[FS_MAX_PATH] = {0};
                strcpy(amiibo_path, path.c_str());
                if(IsActiveAmiiboValid()) {
                    if(strcmp(g_ActiveVirtualAmiiboPath, amiibo_path) == 0) {
                        // User selected the active amiibo, so let's change connection then
                        auto status = emu::GetActiveVirtualAmiiboStatus();
                        switch(status) {
                            case emu::VirtualAmiiboStatus::Connected: {
                                emu::SetActiveVirtualAmiiboStatus(emu::VirtualAmiiboStatus::Disconnected);
                                root_frame->setSubtitle(MakeStatusText());
                                break;
                            }
                            case emu::VirtualAmiiboStatus::Disconnected: {
                                emu::SetActiveVirtualAmiiboStatus(emu::VirtualAmiiboStatus::Connected);
                                root_frame->setSubtitle(MakeStatusText());
                                break;
                            }
                            default:
                                break;
                        }
                        return true;
                    }
                }
                // Set active amiibo and update our active amiibo value
                emu::SetActiveVirtualAmiibo(amiibo_path, sizeof(amiibo_path));
                UpdateActiveAmiibo();
                selected_header->setText(MakeActiveAmiiboText());
                root_frame->setSubtitle(MakeStatusText());
                return true;
            }
            return false;
        }

        virtual tsl::elm::Element *createUI() override {
            list = new tsl::elm::List();
            header_list = new tsl::elm::List();

            u32 count = 0;
            tsl::hlp::doWithSDCardHandle([&](){
                auto dir = opendir(this->amiibo_path.c_str());
                if(dir) {
                    while(true) {
                        auto entry = readdir(dir);
                        if(entry == nullptr) {
                            break;
                        }
                        char path[FS_MAX_PATH] = {0};
                        auto str_path = this->amiibo_path + "/" + entry->d_name;
                        strcpy(path, str_path.c_str());
                        // Find virtual amiibo
                        emu::VirtualAmiiboData data = {};
                        if(R_SUCCEEDED(emu::TryParseVirtualAmiibo(path, sizeof(path), &data))) {
                            auto item = new tsl::elm::SmallListItem(data.name);
                            item->setClickListener(std::bind(&AmiiboList::OnItemClick, this, std::placeholders::_1, str_path));
                            list->addItem(item);
                            count++;
                        }
                    }
                    closedir(dir);
                }
            });

            selected_header = new tsl::elm::BigCategoryHeader(MakeActiveAmiiboText(), true);
            count_header = new tsl::elm::CategoryHeader(TR("Available virtual amiibos")+ " (" + std::to_string(count) + ")", true);

            header_list->addItem(selected_header);
            header_list->addItem(count_header);

            root_frame->setHeader(header_list);
            root_frame->setContent(list);
            return root_frame;
        }

        virtual void update() override {
            if(UpdateCurrentApplicationIntercepted()) {
                root_frame->setSubtitle(MakeStatusText());
            }
        }

};

class CategoryList : public tsl::Gui {

    private:
        tsl::elm::CustomOverlayFrame *root_frame;
        tsl::elm::BigCategoryHeader *selected_header;
        tsl::elm::CategoryHeader *count_header;
        tsl::elm::List *list;
        tsl::elm::List *header_list;

    public:
        CategoryList() : root_frame(new tsl::elm::CustomOverlayFrame(MakeTitleText(), MakeStatusText())), selected_header(new tsl::elm::BigCategoryHeader(MakeActiveAmiiboText(), true)) {}

        void Refresh() {
            this->root_frame->setSubtitle(MakeStatusText());
            this->selected_header->setText(MakeActiveAmiiboText());
        }

        static bool OnItemClick(u64 keys, const std::string &path) {
            if(keys & KEY_A) {
                tsl::changeTo<AmiiboList>(path);
                g_NeedsUpdateCategoryList = true;
                return true;
            }
            return false;
        }

        virtual tsl::elm::Element *createUI() override {
            this->list = new tsl::elm::List();
            this->header_list = new tsl::elm::List();

            // Root
            auto root_item = new tsl::elm::SmallListItem("<root>");
            root_item->setClickListener(std::bind(&CategoryList::OnItemClick, std::placeholders::_1, g_VirtualAmiiboDirectory));
            this->list->addItem(root_item);

            u32 count = 1; // Root
            tsl::hlp::doWithSDCardHandle([&](){
                auto dir = opendir(g_VirtualAmiiboDirectory);
                if(dir) {
                    while(true) {
                        auto entry = readdir(dir);
                        if(entry == nullptr) {
                            break;
                        }
                        char path[FS_MAX_PATH] = {0};
                        auto str_path = std::string(g_VirtualAmiiboDirectory) + "/" + entry->d_name;
                        strcpy(path, str_path.c_str());
                        // If it's a valid amiibo, skip
                        emu::VirtualAmiiboData tmp_data;
                        if(R_SUCCEEDED(emu::TryParseVirtualAmiibo(path, sizeof(path), &tmp_data))) {
                            continue;
                        }
                        if(entry->d_type & DT_DIR) {
                            auto item = new tsl::elm::SmallListItem(entry->d_name);
                            item->setClickListener(std::bind(&CategoryList::OnItemClick, std::placeholders::_1, str_path));
                            this->list->addItem(item);
                            count++;
                        }
                    }
                    closedir(dir);
                }
            });

            this->count_header = new tsl::elm::CategoryHeader(TR("Available categories") + " (" + std::to_string(count) + ")", true);

            header_list->addItem(this->selected_header);
            header_list->addItem(this->count_header);

            this->root_frame->setHeader(this->header_list);
            this->root_frame->setContent(list);
            return this->root_frame;
        }

        virtual void update() override {
            bool upd = false;
            if(g_NeedsUpdateCategoryList) {
                upd = true;
                g_NeedsUpdateCategoryList = false;
            }
            if(UpdateCurrentApplicationIntercepted()) {
                upd = true;
            }
            if(upd) {
                this->Refresh();
            }
        }

};

class MainGui : public tsl::Gui {

    private:
        tsl::elm::BigCategoryHeader *amiibo_header;
        tsl::elm::OverlayFrame *root_frame;

    public:
        MainGui() : amiibo_header(new tsl::elm::BigCategoryHeader(MakeActiveAmiiboText(), true)), root_frame(new tsl::elm::OverlayFrame(MakeTitleText(), MakeStatusText())) {}

        void Refresh() {
            this->root_frame->setSubtitle(MakeStatusText());
            this->amiibo_header->setText(MakeActiveAmiiboText());
        }

        virtual tsl::elm::Element *createUI() override {
            auto list = new tsl::elm::List();

            if(g_Initialized) {
                auto status = emu::GetEmulationStatus();

                auto *toggle_item = new tsl::elm::NamedStepTrackBar("\u22EF", { TR("Off"), TR("On") });
                auto *select_item = new tsl::elm::SmallListItem(TR("View amiibo"));

                u8 toggle_progress;
                switch(status) {
                    case emu::EmulationStatus::On:
                        toggle_progress = 1;
                        break;
                    case emu::EmulationStatus::Off:
                        toggle_progress = 0;
                        break;
                }
                toggle_item->setProgress(toggle_progress);

                toggle_item->setValueChangedListener([&](u8 progress) {
                    switch(progress) {
                        case 1: {
                            emu::SetEmulationStatus(emu::EmulationStatus::On);
                            break;
                        }
                        case 0: {
                            emu::SetEmulationStatus(emu::EmulationStatus::Off);
                            break;
                        }
                    }
                    this->Refresh();
                });

                select_item->setClickListener([](u64 keys) {
                    if(keys & KEY_A) {
                        tsl::changeTo<CategoryList>();
                        g_NeedsUpdateMainGui = true;
                        return true;
                    }
                    return false;
                });

                list->addItem(new tsl::elm::BigCategoryHeader(TR("Manage emulation (on / off)"), true));
                list->addItem(toggle_item);
                list->addItem(this->amiibo_header);
                list->addItem(select_item);
            }
            else {
                list->addItem(new tsl::elm::BigCategoryHeader("...", true));
            }

            this->root_frame->setContent(list);
            return this->root_frame;
        }

        virtual void update() override {
            bool upd = false;
            if(g_NeedsUpdateMainGui) {
                upd = true;
                g_NeedsUpdateMainGui = false;
            }
            if(UpdateCurrentApplicationIntercepted()) {
                upd = true;
            }
            if(upd) {
                this->Refresh();
            }
        }

};

class Overlay : public tsl::Overlay {

    public:
        virtual void initServices() override {
            tsl::hlp::doWithSmSession([&] {
                if(emu::IsAvailable()) {
                    g_Initialized = R_SUCCEEDED(emu::Initialize()) && R_SUCCEEDED(pmdmntInitialize()) && R_SUCCEEDED(pminfoInitialize());
                    if(g_Initialized) {
                        g_Version = emu::GetVersion();
                        emu::GetVirtualAmiiboDirectory(g_VirtualAmiiboDirectory, sizeof(g_VirtualAmiiboDirectory));
                    }
                }
            });
            if(g_Initialized) {
                UpdateActiveAmiibo();
            }
            Translator::global().loadSystem();
        }

        virtual void exitServices() override {
            pminfoExit();
            pmdmntExit();
            emu::Exit();
        }

        virtual std::unique_ptr<tsl::Gui> loadInitialGui() override {
            return initially<MainGui>();
        }

};

int main(int argc, char **argv) {
    return tsl::loop<Overlay>(argc, argv);
}
