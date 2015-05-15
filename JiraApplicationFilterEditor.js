function BmInitJiraApplicationFilterEditor(o) {
    $('#' + o.ctlProject).select2({
        data: o.data
    });
}