// JavaScript function to invoke a .NET method
window.callDotNetMethod = (dotnetHelper) => {
    dotnetHelper.invokeMethodAsync('NuGetPackageManagerApp.Pages.FilePickerPage', 'GetFoundNuGetPackages', 'parameter')
        .then(result => {
            console.log('Result from .NET:', result);
        })
        .catch(error => {
            console.error('Error invoking .NET method:', error);
        });
};
