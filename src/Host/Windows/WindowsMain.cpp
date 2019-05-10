#pragma once

using namespace Windows::ApplicationModel;
using namespace Windows::ApplicationModel::Core;
using namespace Windows::ApplicationModel::Activation;
using namespace Windows::UI::Core;
using namespace Windows::UI::ViewManagement;
using namespace Windows::Foundation;
using namespace Windows::System;
using namespace Windows::Graphics::Display;

// TODO: See https://github.com/Microsoft/Xbox-ATG-Samples/blob/master/XDKSamples/IntroGraphics/SimpleTriangleCppWinRT12/Main.cpp

class MainApplicationView : public implements<MainApplicationView, IFrameworkViewSource, IFrameworkView>
{
private:
	bool isRunning;
    bool isVisible;
    bool isSizeMove;
    float systemDpi;
	int logicalWidth;
	int logicalHeight;
    WindowsCoreEngineHost* coreEngineHost;

    inline int ConvertDipsToPixels(float dips) const
    {
        return int(dips * this->systemDpi / 96.0f + 0.5f);
    }

    inline float ConvertPixelsToDips(int pixels) const
    {
        return (float(pixels) * 96.0f / this->systemDpi);
    }

    void HandleWindowSizeChanged()
    {
        // TODO: It seems that when we switch monitor dpichanged is not called

        int outputWidth = ConvertDipsToPixels(this->logicalWidth);
        int outputHeight = ConvertDipsToPixels(this->logicalHeight);

        //m_sample->OnWindowSizeChanged(outputWidth, outputHeight, rotation);
    }

public:
    MainApplicationView()
    {
		this->isRunning = true;
        this->isVisible = true;
        this->systemDpi = 96.0f;
    }

    // IFrameworkView methods
    void Initialize(const CoreApplicationView& applicationView)
    {
        Size desiredSize = Size(1280, 720);

        ApplicationView::PreferredLaunchViewSize(desiredSize);
        ApplicationView::PreferredLaunchWindowingMode(ApplicationViewWindowingMode::PreferredLaunchViewSize);

        applicationView.Activated({ this, &MainApplicationView::OnActivated });

        CoreApplication::Suspending({ this, &MainApplicationView::OnSuspending });
        CoreApplication::Resuming({ this, &MainApplicationView::OnResuming });
    }

    void Uninitialize()
    {

    }

	void SetWindow(const CoreWindow& window)
    {
        window.SizeChanged({ this, &MainApplicationView::OnWindowSizeChanged });
        window.VisibilityChanged({ this, &MainApplicationView::OnVisibilityChanged });

        window.ResizeStarted([this](auto&&, auto&&) 
        { 
            this->isSizeMove = true; 
        });

        window.ResizeCompleted([this](auto&&, auto&&) 
        { 
            this->isSizeMove = false; 
            HandleWindowSizeChanged(); 
        });

        window.Closed([this](auto&&, auto&&) 
        { 
            this->isRunning = true; 
        });

        auto currentDisplayInformation = DisplayInformation::GetForCurrentView();

        currentDisplayInformation.DpiChanged({ this, &MainApplicationView::OnDpiChanged });
        DisplayInformation::DisplayContentsInvalidated({ this, &MainApplicationView::OnDisplayContentsInvalidated });

        //m_sample->Initialize(windowPtr, outputWidth, outputHeight, rotation);
    }

    void Load(const hstring& entryPoint)
    {
    }

	void Run()
    {
		while (this->isRunning)
		{
            if (this->isVisible)
            {
                // TODO: Engine Update

                CoreWindow::GetForCurrentThread().Dispatcher().ProcessEvents(CoreProcessEventsOption::ProcessAllIfPresent);
            }
            else
            {
                CoreWindow::GetForCurrentThread().Dispatcher().ProcessEvents(CoreProcessEventsOption::ProcessOneAndAllPending);
            }
		}
	}

    void OnActivated(const CoreApplicationView& applicationView, const IActivatedEventArgs& args)
    {
        this->systemDpi = DisplayInformation::GetForCurrentView().LogicalDpi();
        
        this->logicalWidth = CoreWindow::GetForCurrentThread().Bounds().Width;
		this->logicalHeight = CoreWindow::GetForCurrentThread().Bounds().Height;

        this->coreEngineHost = new WindowsCoreEngineHost();
        this->coreEngineHost->StartEngine(hstring());
    }

	void OnSuspending(const IInspectable& sender, const SuspendingEventArgs& args)
	{
		// auto deferral = args.SuspendingOperation().GetDeferral();

		// auto f = std::async(std::launch::async, [this, deferral]()
		// 	{
		// 		m_sample->OnSuspending();

		// 		deferral.Complete();
		// 	});
	}

	void OnResuming(const IInspectable& sender, const IInspectable& args)
	{
		// TODO: Engine Resource resuming
	}

    void OnDpiChanged(const DisplayInformation& sender, const IInspectable& args)
    {
        this->systemDpi = sender.LogicalDpi();

        HandleWindowSizeChanged();

        //Mouse::SetDpi(m_DPI);
    }

    void OnDisplayContentsInvalidated(const DisplayInformation& sender, const IInspectable& args)
    {
        //m_sample->ValidateDevice();
    }

	void OnWindowSizeChanged(const CoreWindow& sender, const WindowSizeChangedEventArgs& args)
	{
		this->logicalWidth = sender.Bounds().Width;
		this->logicalHeight = sender.Bounds().Height;

		if (this->isSizeMove)
        {
			return;
        }

		HandleWindowSizeChanged();
	}

    void OnVisibilityChanged(const CoreWindow& sender, const VisibilityChangedEventArgs& args)
    {
        this->isVisible = args.Visible();

        if (this->isVisible)
        {
            // TODO: Engine Resource activated
        }

        else
        {
            // TODO: Engine Resource deactivated
        }
    }

    IFrameworkView CreateView()
    {
        return *this;
    }
};

int __stdcall wWinMain(HINSTANCE applicationInstance, HINSTANCE, PWSTR, int)
{
	winrt::init_apartment();

	OutputDebugString("CoreEngine Windows Host\n");

	// hstring appName = {};

    // if (argc > 1)
    // {
    //     appName = { argv[1] };
    // }

	MainApplicationView mainApplicationView = MainApplicationView();
	CoreApplication::Run(mainApplicationView);

	// WindowsCoreEngineHost* coreEngineHost = new WindowsCoreEngineHost();
    // coreEngineHost->StartEngine(appName);

    // // TODO: To remove, this is only used for debugging with console messages

    // coreEngineHost->UpdateEngine(5);


    coreEngineHost->UpdateEngine(5);


	OutputDebugString("CoreEngine Windows Host has ended.\n");
	winrt::uninit_apartment();

	return 0;
}